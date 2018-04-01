local BTConnection = bt.Class("BTConnection")
bt.BTConnection = BTConnection

function BTConnection:ctor()
    self.name = "BTConnection"
    self.sourceNode = nil
    self.targetNode = nil
    self.isDisabled = false
    self.status = bt.Status.Resting
end

function BTConnection:setActive(flag)
    if not self.isDisabled and not flag then
        self:reset()
    end
    self.isDisabled = not flag
end

function BTConnection:isActive()
    return not self.isDisabled
end

function BTConnection:create(source,target)
    if source == nil or target == nil then
        print("Can't Create a Connection without providing Source and Target Nodes")
        return nil
    end
    self.sourceNode = source
    self.targetNode = target
end

function BTConnection:execute(agent,blackboard)
    if not self.isActive() then
        return bt.Status.Resting
    end
    self.status = targetNode:execute(agent,blackboard)
    return self.status
end

function BTConnection:reset(recursively)
    if self.status == bt.Status.Resting then
        return
    end
    self.status = bt.Status.Resting
    if recursively then
        targetNode:reset(recursively)
    end
end